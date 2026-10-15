class GitFlowNext < Formula
  desc "Modern implementation of the Git-flow branching model"
  homepage "https://git-flow.sh/"
  url "https://github.com/gittower/git-flow-next/releases/download/v2.1.0/git-flow-next-v2.1.0-windows-amd64.zip"
  sha256 "9bfbc2746ea647fe664e0a5be375f7d7e144412d5031064f60efb1f8fbe1b656"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gittower/git-flow-next/archive/refs/tags/v2.1.0.tar.gz"
  end

  def install
    bin.install "git-flow.exe"
  end

  test do
    system "git", "init"
    system "git", "config", "user.name", "BrewTestBot"
    system "git", "config", "user.email", "BrewTestBot@test.com"
    system "git", "flow", "init", "--defaults"
    system "git", "flow", "config"
    assert_equal "develop", shell_output("git symbolic-ref --short HEAD").chomp
    assert_match version.to_s, shell_output("#{bin}/git-flow version")
  end
end
