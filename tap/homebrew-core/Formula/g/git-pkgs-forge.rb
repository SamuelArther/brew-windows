class GitPkgsForge < Formula
  desc "Go library and CLI for working with git forges"
  homepage "https://github.com/git-pkgs/forge"
  url "https://github.com/git-pkgs/forge/releases/download/v0.10.1/forge_0.10.1_windows_amd64.tar.gz"
  sha256 "ac7539356af53c910554f1cd115fd247f518510a59653aa6350b452885445fb9"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/git-pkgs/forge/archive/refs/tags/v0.10.1.tar.gz"
  end

  def install
    bin.install "forge.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/forge version")

    output = shell_output("#{bin}/forge repo view 2>&1", 1)
    assert_match "Error: reading remote \"origin\"", output
  end
end
