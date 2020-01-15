## scripts/circleci/

These scripts are used when you are running a build that uses CircleCI. See the [main README](../../README.md#circleci-builds) for details on how to set this up in your configuration. This file describes the internal workings of the CircleCI integration.

If you specified any combination of `linux`, `mac`, or `windows` in the `circleCI` section, Releaser follows this procedure:

1. Create a new branch off of the release branch called `release-$BRANCH_NAME`, e.g. if you are releasing from master then it is `release-master`. Any previous branch with this name is deleted first. In the checkout of this branch:
2. Copy any changes that were already made by the `update-version` step.
3. Copy `scripts/circleci` into `.ldrelease/circleci`.
4. If you are also using a project template, copy `scripts/project-templates/$TEMPLATE_NAME` into `.ldrelease/circleci/template`.
5. Generate a new `.circleci/config.yml` file using the template in `templates/circleci-config.yml`. This contains all the workflow and job configuration needed to run all of your selected host types, including all environment variables that will be passed to the build. Each job will execute the appropriate `execute.sh` or `execute.ps1` script once for each build step, with the name of the step as an argument.
6. Commit and push to this temporary branch. This should trigger one CircleCI job for each selected host type. Wait for the job to run and get its output.
7. If all of the jobs succeeded, then also commit and push the changes to the main project repo (i.e. the new changelog text, plus the changes made by `update-version`).
8. Attach any artifacts that the CircleCI jobs stored in `artifacts/` to the Github release.

The logic in the `execute.sh` or `execute.ps1` script just iterates through the steps in the space-delimited list `$LD_RELEASE_CIRCLECI_STEPS`, and executes whichever version of the step script is found first (if any): `.ldrelease/$HOST_TYPE-$NAME`, `.ldrelease/$NAME`, or `.ldrelease/circleci/template/$NAME`.
