class Tcpkali < Formula
  desc "High performance TCP and WebSocket load generator and sink"
  homepage "https://web.archive.org/web/20230303212249/https://github.com/satori-com/tcpkali"
  url "https://www.mirrorservice.org/sites/distfiles.macports.org/tcpkali/tcpkali-1.1.1.tar.gz"
  sha256 "a9a15a1703fc4960360a414ee282d821a7b42d4bbba89f9e72a796164ff69598"
  license "BSD-2-Clause"

  deprecate! date: "2025-03-19", because: :repo_removed
  disable! date: "2026-03-19", because: :repo_removed

  uses_from_macos "bison" => :build
  uses_from_macos "ncurses"

  def install
    args = []
    if OS.linux?
      inreplace "src/tcpkali_syslimits.c", "<sys/sysctl.h>", "<linux/sysctl.h>"
      # Help old config scripts identify arm64 linux
      args << "--build=aarch64-unknown-linux-gnu" if Hardware::CPU.arm? && Hardware::CPU.is_64_bit?
    end

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"tcpkali", "-l1237", "-T0.5", "127.1:1237"
  end
end
