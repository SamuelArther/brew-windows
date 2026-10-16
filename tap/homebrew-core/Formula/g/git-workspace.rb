class GitWorkspace < Formula
  desc "Sync personal and work git repositories from multiple providers"
  homepage "https://github.com/orf/git-workspace"
  url "https://github.com/orf/git-workspace/releases/download/v1.5.0/git-workspace-Windows-x86_64.zip"
  sha256 "66a4e5a26d4dbffaddbc95bdbf30b0d9ff282981128849e37edd3799c70f4419"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/orf/git-workspace/archive/refs/tags/v1.11.0.tar.gz"
  end

  def install
    bin.install "git-workspace.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-workspace --version 2>&1")
  end
end
