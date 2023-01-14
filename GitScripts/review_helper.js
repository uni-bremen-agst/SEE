/**
  * Converts the output of the Python bad pattern detector into
  * valid JSON for Octokit.
  * TODO: Just return valid JSON directly from Python script.
  */

module.exports = {
    to_comments: (patterns) => {
        let comments = [];
        let comment = {};
        console.assert(patterns.length % 6 === 0);
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
                    comment['body'] += patterns[i];
                    break;
                case 4: // suggestion, if there is one.
                    if (patterns[i] !== '') {
                        comment['body'] += `\n\n\`\`\`suggestion\n${patterns[i]}\n\`\`\``;
                    }
                    break;
                case 5: // regex triggering this bad pattern
                    comment['body'] += `\n---\n> This bad pattern was triggered by the regular expression \`${patterns[i]}\``;
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
    dismiss_old_reviews: (github, context) => {
        // First, find out who we are.
        let user_id = github.rest.users.getAuthenticated()['id'];

        // Then, we find our reviews for this pull request.
        let review_ids = github.rest.pulls.listReviews({
            owner: context.repo.owner,
            repo: context.repo.repo,
            pull_number: context.issue.number
        }).filter(x => x['user']['id'] === user_id).map(x => x['id']);

        for (let review_id of review_ids) {
            github.rest.pulls.dismissReview({
                owner: context.repo.owner,
                repo: context.repo.repo,
                pull_number: context.issue.number,
                review_id: review_id,
                message: 'Review has become outdated. New review follows.'
            });
        }
    }
}

