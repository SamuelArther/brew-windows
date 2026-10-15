class Heroku < Formula
  desc "Command-line interface for the Heroku platform"
  homepage "https://devcenter.heroku.com/articles/heroku-cli"
  url "https://cli-assets.heroku.com/versions/11.12.0/bb55b3c/heroku-v11.12.0-bb55b3c-win32-x64.tar.gz"
  version "11.12.0"
  sha256 "4d600a4500b8d55bb1d8e2af0f32f5e457b29b3615cb98f487c812d43468ef90"
  license "ISC"
  depends_on "git"

  # Official standalone CLI with bundled Node.js. Login/deployment are never run during installation.

  def install
    install_tree
    windows_cmd "libexec/bin/heroku.cmd", "heroku"
  end
end
