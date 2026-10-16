class Rmcast < Formula
  desc "IP Multicast library"
  homepage "https://web.archive.org/web/20250722003825/https://www.land.ufrj.br/tools/rmcast/rmcast.html"
  url "https://web.archive.org/web/20250621130503/https://www.land.ufrj.br/tools/rmcast/download/rmcast-2.0.0.tar.gz"
  sha256 "79ccbdbe4a299fd122521574eaf9b3e2d524dd5e074d9bc3eb521f1d934a59b1"
  license "QPL-1.0"

  # 503 error for https://www.land.ufrj.br
  deprecate! date: "2025-09-15", because: :repo_removed
  disable! date: "2026-09-15", because: :repo_removed

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build

  deny_network_access!

  def install
    # Run autoreconf to regenerate the configure script and update outdated macros.
    # This ensures that the build system is properly configured on both macOS
    # (avoiding issues like flat namespace conflicts) and Linux (where outdated
    # config scripts may fail to detect the correct build type).
    system "autoreconf", "--force", "--install", "--verbose"

    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    false
  end
end
