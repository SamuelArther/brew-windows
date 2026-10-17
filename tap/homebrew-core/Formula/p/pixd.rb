class Pixd < Formula
  desc "Visual binary data using a colour palette"
  homepage "https://github.com/FireyFly/pixd"
  url "https://github.com/FireyFly/pixd/archive/refs/tags/v1.0.0.tar.gz"
  sha256 "011440a8d191e40a572910b0ce7a094e9b4ee75cf972abc6d30674348edf4158"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/pixd-1.0.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f95104d01e27e54191494fdf69f4f6a2ac901b2bf935b3afa34d77fe4adb63e9"
  end

  def install
    bin.mkdir
    man1.mkpath

    # BSD install does not understand the GNU "-D" flag.
    inreplace "Makefile", "install -D", "install"

    system "make"
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    (testpath/"test.txt").write "H"

    assert_match \
      "0000 \e[0m\e[38;2;147;221;0m▀\e[m",
      shell_output("#{bin}/pixd test.txt")
  end
end
