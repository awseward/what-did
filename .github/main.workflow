workflow "Push to heroku from master" {
  on = "push"
  resolves = ["verify-production"]
}

action "master-only" {
  uses = "actions/bin/filter@master"
  args = "branch master"
}

action "container-login" {
  uses = "actions/heroku@master"
  needs = ["master-only"]
  secrets = ["HEROKU_API_KEY"]

  args = "container:login"
}

action "push-production" {
  uses = "actions/heroku@master"
  needs = ["container-login"]
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did"
  }

  args = ["container:push", "web", "--recursive", "--app", "$HEROKU_APP"]
}

action "release-production" {
  needs = ["push-production"]
  uses = "actions/heroku@master"
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did"
  }

  args = ["container:release", "web", "--app", "$HEROKU_APP"]
}

action "verify-production" {
  needs = ["release-production"]
  uses = "actions/heroku@master"
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did"
  }

  args = ["apps:info", "$HEROKU_APP"]
}
