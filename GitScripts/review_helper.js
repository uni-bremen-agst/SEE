/**
 * Various helper functions for the automatic review process
 * present as part of GitHub Actions.
 */


// Returns all reviews made by us (i.e., GitHub Actions).
async function get_reviews(github, context) {
  // We find our review comments for this pull request.
  const reviews = await github.paginate(github.rest.pulls.listReviewComments,
    {
      owner: context.repo.owner,
      repo: context.repo.repo,
      pull_number: context.issue.number
    });
  return reviews.filter(x => x['user']['login'] === "github-actions[bot]");
}

module.exports = {
  // Filters out all comments from `comments` which we already made.
  filter_out_existing_comments: async (github, context, comments) => {
    // Uniqueness is now determined by path, line number, and comment text.
    // To make sure the Set's `has` method works by comparing array values
    // rather than identity, it seems we need to use this JSON.stringify approach.
    const reviews = await get_reviews(github, context);
    let existing = new Set(reviews.map(x => [x['path'], x['line'] !== null ? x['line'] : x['original_line'], x['body']]).map(JSON.stringify));
    for (let i = comments.length - 1; i >= 0; i--) {
      const comment = comments[i];
      if (existing.has(JSON.stringify([comment['path'], comment['line'], comment['body']]))) {
        // Remove comment, we already posted it at the same position.
        console.log("Removing existing comment " + JSON.stringify(comment));
        comments.splice(i, 1);
      }
    }
  }
}

