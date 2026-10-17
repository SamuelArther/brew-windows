class Rinetd < Formula
  desc "Internet TCP redirection server"
  homepage "https://github.com/samhocevar/rinetd"
  url "https://github.com/samhocevar/rinetd/releases/download/v0.73/rinetd-0.73.tar.bz2"
  sha256 "39180d31b15f059b2e876496286356e40183d1567c2e2aec41aacad8721ecc44"
  license "GPL-2.0-or-later"
  revision 1
  # NOTE: Original (unversioned) tool is at https://github.com/boutell/rinetd
  #       Debian tracks the "samhocevar" fork so we follow suit
  head "https://github.com/samhocevar/rinetd.git", branch: "main"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "2b001d7fcf0075492fc018c697c0543c47c7a20bf09f012021794ac4b9a0bc04"
  end

  def install
    # The daemon() function does exist but its deprecated so keep configure
    # away:
    system "./configure", "--prefix=#{prefix}", "--sysconfdir=#{share}", "ac_cv_func_daemon=no"

    # Point hardcoded runtime paths inside of our prefix
    inreplace "src/rinetd.h" do |s|
      s.gsub! "/etc/rinetd.conf", "#{etc}/rinetd.conf"
      s.gsub! "/var/run/rinetd.pid", "#{var}/run/rinetd.pid"
    end
    inreplace "rinetd.conf", "/var/log", "#{var}/log"

    # Install conf file only as example and let etc.install handle existing config
    cp "rinetd.conf", "rinetd.conf.example"
    etc.install "rinetd.conf"
    inreplace "Makefile", "rinetd.conf", "rinetd.conf.example"

    system "make", "install"
  end

  service do
    run [opt_sbin/"rinetd", "-c", etc/"rinetd.conf", "-f"]
  end

  test do
    system sbin/"rinetd", "-h"
  end
end
