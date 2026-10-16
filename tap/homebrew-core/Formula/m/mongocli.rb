class Mongocli < Formula
  desc "MongoDB CLI enables you to manage your MongoDB in the Cloud"
  homepage "https://www.mongodb.com/docs/mongocli/current/"
  url "https://github.com/mongodb/mongodb-cli/releases/download/mongocli/v2.0.9/mongocli_2.0.9_windows_x86_64.zip"
  version "2.0.9"
  sha256 "bf550c2fc2dd7d160a683743ef4cb3e9d4448c0bb1316809cd8f66040a407b03"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mongodb/mongodb-cli/archive/refs/tags/mongocli/v2.0.9.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/mongocli.exe", "mongocli"
  end

  test do
    assert_match "mongocli version: #{version}", shell_output("#{bin}/mongocli --version")
    assert_match "Error: this action requires authentication", shell_output("#{bin}/mongocli iam projects ls 2>&1", 1)
    assert_match "PROFILE NAME", shell_output("#{bin}/mongocli config ls")
  end
end
