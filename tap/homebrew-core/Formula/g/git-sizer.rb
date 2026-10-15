class GitSizer < Formula
  desc "Compute various size metrics for a Git repository"
  homepage "https://github.com/github/git-sizer"
  url "https://github.com/github/git-sizer/releases/download/v1.5.0/git-sizer-1.5.0-windows-amd64.zip"
  sha256 "52093c1cba0bb8e00da14c9eef678eb052fc729c32419415817076f06b5c85d8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/github/git-sizer/archive/refs/tags/v1.5.0.tar.gz"
  end

  def install
    bin.install "git-sizer.exe"
  end

  test do
    system "git", "init"
    output = shell_output(bin/"git-sizer")
    assert_match "No problems above the current threshold were found", output
  end
end
