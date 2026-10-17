class Kommit < Formula
  desc "More detailed commit messages without committing!"
  homepage "https://github.com/vigo/kommit"
  url "https://github.com/vigo/kommit/archive/refs/tags/v1.1.0.tar.gz"
  sha256 "c51e87c9719574feb9841fdcbd6d1a43b73a45afeca25e1312d2699fdf730161"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "77eeaaec498be8f296e86c19a0a2beaf4b2809a7132a5b7ba34f8f6a6800533a"
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
