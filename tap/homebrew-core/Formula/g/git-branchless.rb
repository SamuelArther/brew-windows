class GitBranchless < Formula
  desc "High-velocity, monorepo-scale workflow for Git"
  homepage "https://github.com/arxanas/git-branchless"
  url "https://github.com/arxanas/git-branchless/releases/download/v0.11.1/git-branchless-v0.11.1-x86_64-pc-windows-msvc.zip"
  sha256 "94b3497fa98f0ca3a2bf0a2d089349843bc2a2103096cc008d8b397576bb5325"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/arxanas/git-branchless/archive/refs/tags/v0.11.1.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "git-branchless.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-branchless --version 2>&1")
  end
end
