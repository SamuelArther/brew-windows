class John < Formula
  desc "Featureful UNIX password cracker"
  homepage "https://www.openwall.com/john/"
  url "https://www.openwall.com/john/k/john-1.9.0.tar.xz"
  sha256 "0b266adcfef8c11eed690187e71494baea539efbd632fe221181063ba09508df"
  license "GPL-2.0-or-later"
  revision 1

  livecheck do
    url :homepage
    regex(/href=.*?john[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "c3fa582fa2892ad2fa2a60e6cccecd83b65d052bac901d1a83bcfb12190cc722"
  end

  uses_from_macos "libxcrypt"

  conflicts_with "john-jumbo", because: "both install the same binaries"

  # Backport of official patch from jumbo fork (https://www.openwall.com/lists/john-users/2016/01/04/1)
  patch do
    file "Patches/john/1.9.0.patch"
  end

  deny_network_access!

  def install
    inreplace "src/params.h" do |s|
      s.gsub!(/#define JOHN_SYSTEMWIDE[[:space:]]*0/, "#define JOHN_SYSTEMWIDE 1")
      s.gsub!(/#define JOHN_SYSTEMWIDE_EXEC.*/, "#define JOHN_SYSTEMWIDE_EXEC \"#{pkgshare}\"")
      s.gsub!(/#define JOHN_SYSTEMWIDE_HOME.*/, "#define JOHN_SYSTEMWIDE_HOME \"#{pkgshare}\"")
    end

    ENV.deparallelize

    target = if OS.mac?
      "macosx-x86-64"
    else
      "linux-x86-64"
    end

    system "make", "-C", "src", "clean", "CC=#{ENV.cc}", target

    prefix.install "doc/README"
    doc.install Dir["doc/*"]
    %w[john unafs unique unshadow].each do |b|
      bin.install "run/#{b}"
    end
    pkgshare.install Dir["run/*"]
  end

  test do
    (testpath/"passwd").write <<~EOS
      root:$1$brew$dOoH2.7QsPufgT8T.pihw/:0:0:System Administrator:/var/root:/bin/sh
    EOS
    system bin/"john", "--wordlist=#{pkgshare}/password.lst", "passwd"
    assert_match(/snoopy/, shell_output("#{bin}/john --show passwd"))
  end
end
