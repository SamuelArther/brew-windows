class Vcprompt < Formula
  desc "Provide version control info in shell prompts"
  homepage "https://github.com/powerman/vcprompt"
  url "https://github.com/powerman/vcprompt/archive/refs/tags/v1.3.0.tar.gz"
  sha256 "3db5ebad2e333d43b464b665c8d43b35156b0f144052f10c340a5c5007a6874d"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "149c3981e53d8b6e75b2f70e9c4b424ebef206d8612e71f02a3a4afae54de9c5"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "sqlite"

  def install
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", *std_configure_args
    system "make", "PREFIX=#{prefix}",
                   "MANDIR=#{man1}",
                   "install"
  end

  test do
    system bin/"vcprompt"
  end
end
