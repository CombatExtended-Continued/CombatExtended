const core = require('@actions/core');
const github = require('@actions/github');

const { Octokit } = require("@octokit/action");
const octokit = new Octokit();

try {
    // `who-to-greet` input defined in action metadata file
    const issue = core.getInput('issue-number');
    const runId = core.getInput('run-id');
    const [owner, repo] = process.env.GITHUB_REPOSITORY.split("/");

    console.log(`get-artifact.js started...`);
    console.log(`issue number = ${issue}\nrun id = ${runId}`);
    const { artifacts } = await octokit.request('GET /repos/{owner}/{repo}/actions/runs/{run_id}/artifacts', {
        owner: owner,
        repo: repo
      });

    core.out
    if (artifacts.total_count != 0) {
      var urls = '';
      for(var i = 0;i < artifacts.total_count;i++) {

        urls += artifacts.artifacts[i].archive_download_url + '\t';        
      }
      core.setOutput("urls", urls);
    } else {
      core.setFailed(`no artifacts where found in /repos/${owner}/${repo}/actions/runs/${run_id}/artifacts`);
    }
  } catch (error) {
    core.setFailed(error.message);
  }