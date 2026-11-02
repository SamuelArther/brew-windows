class Kommit < Formula
  desc "More detailed commit messages without committing!"
  homepage "https://github.com/vigo/kommit"
  url "https://github.com/vigo/kommit/archive/refs/tags/v1.1.0.tar.gz"
  sha256 "c51e87c9719574feb9841fdcbd6d1a43b73a45afeca25e1312d2699fdf730161"
  license "MIT"

  deny_network_access!

  bottle do
    sha256 cellar: :any, x86_64_windows: "ef43a35b300330498d1430f45b2424c403c0592ab27f6dfde06705574a911fc7"
  end

  def install
    bin.install "bin/git-kommit"
  end

  test do
    system "git", "init"
    system bin/"git-kommit", "-m", "Hello"
    assert_match "Hello", shell_output("#{bin}/git-kommit -s /dev/null 2>&1")
  end
end
