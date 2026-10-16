class Gitlogue < Formula
  desc "Cinematic Git commit replay tool"
  homepage "https://github.com/unhappychoice/gitlogue"
  url "https://github.com/unhappychoice/gitlogue/releases/download/v0.12.0/gitlogue-v0.12.0-x86_64-pc-windows-msvc.zip"
  sha256 "86bf47a33e6efd01f6d1bfe1d881beef68f1dd44e4573c68672d92069045d144"
  license "ISC"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/unhappychoice/gitlogue/archive/refs/tags/v0.12.0.tar.gz"
  end

  def install
    bin.install "gitlogue.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gitlogue --version 2>&1")
  end
end
