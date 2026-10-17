class Titlecase < Formula
  desc "Script to convert text to title case"
  homepage "http://plasmasturm.org/code/titlecase/"
  url "https://github.com/ap/titlecase/archive/refs/tags/v1.015.tar.gz"
  sha256 "908ef5c40d103200bcc9bd8a55171f20e14d09166aaa556dc91611567d695811"
  license "MIT"
  head "https://github.com/ap/titlecase.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/titlecase-1.015"
    sha256 cellar: :any, x86_64_windows: "51d0fca1fbd70f32dff27ebf86d207c993ff7167b4467fc0d751242bacbe5c7a"
  end

  def install
    bin.install "titlecase"
  end

  test do
    (testpath/"test").write "homebrew"
    assert_equal "Homebrew\n", shell_output("#{bin}/titlecase test")
  end
end
