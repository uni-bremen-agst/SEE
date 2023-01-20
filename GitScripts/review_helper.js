/**
  * Various helper functions for the automatic review process
  * present as part of GitHub Actions.
  */


// Returns all reviews made by us (i.e., GitHub Actions).
async function get_reviews(github, context) {
    // We find our review comments for this pull request.
    const response = await github.rest.pulls.listReviewComments({
        owner: context.repo.owner,
        repo: context.repo.repo,
        pull_number: context.issue.number
    });
    const reviews = await github.paginate(response);
    return reviews.filter(x => x['user']['login'] === "github-actions[bot]");
}

module.exports = {
    // Converts the output of the Python bad pattern detector into
    // valid JSON for Octokit.
    to_comments: (patterns) => {
        let comments = [];
        let comment = {};
        console.assert(patterns.length % 6 === 0, "Bad patterns must consist of six elements each.");
        for (let i = 0; i < patterns.length; i++) {
            switch (i % 6) {
                case 0: // file
                    comment['path'] = patterns[i];
                    break;
                case 1: // line number
                    comment['line'] = Number(patterns[i]);
                    break;
                case 2: // severity level
                    // We prepend the severity emoji to the message.
                    comment['body'] = patterns[i];
                    break;
                case 3: // review comment itself
                    comment['body'] += " " + patterns[i];
                    break;
                case 4: // suggestion, if there is one.
                    if (patterns[i] !== '') {
                        comment['body'] += `\n\n\`\`\`suggestion\n${patterns[i]}\n\`\`\``;
                    }
                    break;
                case 5: // regex triggering this bad pattern
                    comment['body'] += `\n> This bad pattern was triggered by the regular expression \`${patterns[i]}\`. To dismiss this comment manually, please react with the :-1: emoji available in the emoji reaction button to the right.`;
                    // We are also now done with this comment.
                    comments.push(comment);
                    comment = {};
                    break;
                default:
                    // Never happens.
                    console.assert(false);
            }
        }
        return comments;
    },

    // Approves the PR if there are previous review comments,
    // all of which have been resolved (checked by :-1: reaction
    // because resolve status is not queryable via REST API).
    approve: async (github, context, only_if_resolved) => {
        const response = await github.rest.pulls.listReviews({
            owner: context.repo.owner,
            repo: context.repo.repo,
            pull_number: context.issue.number
        });
        let reviews = await github.paginate(response);
        reviews = reviews.filter(x => x['user']['login'] === "github-actions[bot]");
        const comments = await get_reviews(github, context);
        // There must be existing reviews, otherwise we don't need to approve.
        // If `only_if_resolved` is true, we only approve if every thread has
        // been "resolved", otherwise we approve in any case.
        if (reviews.length > 0 && reviews[reviews.length-1]['state'] !== 'APPROVED' && comments.every(x => !only_if_resolved || x['reactions']['-1'] > 0)) {
            console.log("PR looks good now, approving it.");
            await github.rest.pulls.createReview({
                owner: context.repo.owner,
                repo: context.repo.repo,
                pull_number: context.issue.number,
                event: 'APPROVE',
                body: 'Looks good to me now!'
            });
        }
    },

    // Filters out all comments from `comments` which we already made.
    filter_out_existing_comments: async (github, context, comments) => {
        // Uniqueness is now determined by path, line number, and comment text.
        // To make sure the Set's `has` method works by comparing array values
        // rather than identity, it seems we need to use this JSON.stringify approach.
        const reviews = await get_reviews(github, context);
        const existing = new Set(reviews.map(x => [x['path'], x['position'], x['body']])
                                        .map(JSON.stringify));
        let to_remove = [];
        for (let i = comments.length-1; i >= 0; i--) {
            const comment = comments[i];
            if (existing.has(JSON.stringify([comment['path'], comment['line'], comment['body']]))) {
                // Remove comment, we already posted it at the same position.
                console.log("Removing existing comment " + JSON.stringify(comment));
                comments.splice(i, 1);
            }
        }
    }
}

