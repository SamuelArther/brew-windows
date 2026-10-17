class Nyancat < Formula
  desc "Renders an animated, color, ANSI-text loop of the Poptart Cat"
  homepage "https://nyancat.dakko.us/"
  url "https://github.com/klange/nyancat/archive/refs/tags/1.5.2.tar.gz"
  sha256 "88cdcaa9c7134503dd0364a97fa860da3381a09cb555c3aae9918360827c2032"
  license "NCSA"

  # Makefile: Add install directory option
  patch do
    url "https://github.com/klange/nyancat/commit/e11af77f2938ea851f712df62f08de4d369598d4.patch?full_index=1"
    sha256 "24a0772d2725e151b57727ce887f4b3911d19e875785eb7e13a68f4b987831e8"
    type :unofficial
    resolves "https://github.com/klange/nyancat/pull/34"
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/nyancat-1.5.2"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ce52a282c16f3958f6a120d86c805e838327c7cea9fb2b27245f4be6726873fc"
  end

  def install
    system "make"
    system "make", "install", "instdir=#{prefix}"
  end

  test do
    system bin/"nyancat", "--frames", "1", "--width", "40", "--height", "20", "--no-clear"
  end
end
