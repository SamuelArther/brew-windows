class Picocom < Formula
  desc "Minimal dumb-terminal emulation program"
  homepage "https://gitlab.com/wsakernel/picocom"
  url "https://gitlab.com/wsakernel/picocom/-/archive/2024-07/picocom-2024-07.tar.gz"
  sha256 "4379de2ec591a5848123f37ccdbc7fbeee6dd3520ef1ce4119d84202fc268a17"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9a12a150affabd4ee0b7c14f3fe95e1d5b0fd1688bfefa947786a7afa8ac32c9"
  end

  depends_on "go-md2man" => :build

  def install
    system "make", "all", "doc"
    bin.install "picocom"
    man1.install "picocom.1"
  end

  test do
    system bin/"picocom", "--help"
  end
end
