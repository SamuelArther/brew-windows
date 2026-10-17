class Dmagnetic < Formula
  desc "Magnetic Scrolls Interpreter"
  homepage "https://www.dettus.net/dMagnetic/"
  url "https://www.dettus.net/dMagnetic/dMagnetic_0.37.tar.bz2"
  sha256 "ad812bb515bc972e23930d643d5abeaed971d550768b1b3f371bd0f72c3c2e89"
  license "BSD-2-Clause"

  livecheck do
    url :homepage
    regex(/href=.*?dMagnetic[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/dmagnetic-0.37"
    sha256 cellar: :any, x86_64_windows: "f735cfa069c876e663bb18ef918df3d593fdf9ba9788861cf6328bebc50f30c4"
  end

  def install
    # Look for configuration and other data within the Homebrew prefix rather than the default paths
    inreplace "Makefile", "DESTDIR?=/usr/local", "DESTDIR?=$(PREFIX)"
    inreplace "src/frontends/default/pathnames.h" do |s|
      s.gsub! "/etc/", "#{etc}/"
      s.gsub! "/usr/local/", "#{HOMEBREW_PREFIX}/"
    end

    system "make", "PREFIX=#{prefix}", "install"
    (share/"games/dMagnetic").install "testcode/minitest.mag", "testcode/minitest.gfx"
  end

  test do
    args = %W[
      -vmode none
      -vcols 300
      -vrows 300
      -vecho -sres 1024x768
      -mag #{share}/games/dMagnetic/minitest.mag
      -gfx #{share}/games/dMagnetic/minitest.gfx
    ]
    command_output = pipe_output("#{bin}/dMagnetic #{args.join(" ")}", "Hello\n")
    assert_match(/^Virtual machine is running\..*\n> HELLO$/, command_output)
  end
end
