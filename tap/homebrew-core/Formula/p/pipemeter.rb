class Pipemeter < Formula
  desc "Shows speed of data moving from input to output"
  homepage "https://launchpad.net/pipemeter"
  url "https://launchpad.net/pipemeter/trunk/1.1.5/+download/pipemeter-1.1.5.tar.gz"
  sha256 "e470ac5f3e71b5eee1a925d7174a6fa8f0753f2107e067fbca3f383fab2e87d8"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "58aae040191a511865279d9040557f40a3158393d402fd106c8dc5ae03b25aab"
  end

  def install
    system "./configure", "--disable-debug",
                          "--prefix=#{prefix}",
                          "--mandir=#{man}"

    # Fix GNU `install -D` syntax issue
    inreplace "Makefile", "install -Dp -t $(DESTDIR)$(PREFIX)/bin pipemeter",
                          "install -p pipemeter $(PREFIX)/bin"
    inreplace "Makefile", "install -Dp -t $(DESTDIR)$(PREFIX)/man/man1 pipemeter.1",
                          "install -p pipemeter.1 $(PREFIX)/share/man/man1"

    bin.mkpath
    man1.mkpath
    system "make", "install"
  end

  test do
    assert_match "3.00B", pipe_output("#{bin}/pipemeter -r 2>&1 >/dev/null", "foo", 0)
  end
end
