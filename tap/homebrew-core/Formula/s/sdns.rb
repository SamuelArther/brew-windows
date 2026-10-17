class Sdns < Formula
  desc "Privacy important, fast, recursive dns resolver server with dnssec support"
  homepage "https://sdns.dev/"
  url "https://github.com/semihalev/sdns/archive/refs/tags/v1.9.0.tar.gz"
  sha256 "22b2a674276c939f70ef0bc3983001c9ba7a88c758dc640edd154ae2a707795d"
  license "MIT"
  head "https://github.com/semihalev/sdns.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b3ceaca05b719b4b5aecd44d4a4460668ce85615299364ee82f2299da0774e8d"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "make", "build"
    bin.install "sdns"
  end

  service do
    run [opt_bin/"sdns", "--config", etc/"sdns.conf"]
    keep_alive true
    require_root true
    error_log_path var/"log/sdns.log"
    log_path var/"log/sdns.log"
    working_dir opt_prefix
  end

  test do
    require "open3"
    stdout, = Open3.capture3(bin/"sdns", "--config", testpath/"sdns.conf", "--test")
    assert_match "Default config file generated", stdout
    assert_path_exists testpath/"sdns.conf"
  end
end
