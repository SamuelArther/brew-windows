class Ssed < Formula
  desc "Super sed stream editor"
  # Original website is down: https://sed.sourceforge.io/grabbag/ssed/
  homepage "https://packages.debian.org/sid/ssed"
  url "https://deb.debian.org/debian/pool/main/s/ssed/ssed_3.62.orig.tar.gz"
  mirror "https://sed.sourceforge.io/grabbag/ssed/sed-3.62.tar.gz"
  sha256 "af7ff67e052efabf3fd07d967161c39db0480adc7c01f5100a1996fec60b8ec4"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?ssed[._-]v?(\d+(?:\.\d+)+)\.orig\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "2880329d0af13f5fe4bbca80e823d29329d9126862752430f4ac37b73f5e080e"
  end

  def install
    # Fix compile with newer Clang
    ENV.append_to_cflags "-Wno-implicit-function-declaration" if DevelopmentTools.clang_build_version >= 1200

    args = %W[
      --mandir=#{man}
      --infodir=#{info}
      --program-prefix=s
    ]
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm64?

    system "./configure", *args, *std_configure_args
    system "make", "install"
    info.install info/"sed.info" => "ssed.info"
  end

  test do
    assert_equal "homebrew",
      pipe_output("#{bin}/ssed s/neyd/mebr/", "honeydew", 0).chomp
  end
end
