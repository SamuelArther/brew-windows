class Nsq < Formula
  desc "Realtime distributed messaging platform"
  homepage "https://nsq.io/"
  url "https://github.com/nsqio/nsq/archive/refs/tags/v1.3.0.tar.gz"
  sha256 "c6289e295aaa40c8d9651de76e66bc9f23e7f5c40b1cc051ea5901965093e1f0"
  license "MIT"
  head "https://github.com/nsqio/nsq.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "91c284fd2251322a307bf47f16cbd6a5aec22ad1914da82f5791e9097598681b"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "make", "DESTDIR=#{prefix}", "PREFIX=", "install"
  end

  service do
    run [opt_bin/"nsqd", "-data-path=#{var}/nsq"]
    keep_alive true
    working_dir var/"nsq"
    log_path var/"log/nsqd.log"
    error_log_path var/"log/nsqd.error.log"
  end

  test do
    lookupd = spawn bin/"nsqlookupd"
    sleep 2
    d = spawn bin/"nsqd", "--lookupd-tcp-address=127.0.0.1:4160"
    sleep 2
    admin = spawn bin/"nsqadmin", "--lookupd-http-address=127.0.0.1:4161"
    sleep 2
    to_file = spawn bin/"nsq_to_file", "--lookupd-http-address=127.0.0.1:4161",
                                       "--output-dir=#{testpath}",
                                       "--topic=test"
    sleep 2
    system "curl", "-d", "hello", "http://127.0.0.1:4151/pub?topic=test"
    sleep 2
    dat = File.read(Dir["*.dat"].first)
    assert_match "test", dat
    assert_match version.to_s, dat
  ensure
    Process.kill(15, lookupd)
    Process.kill(15, d)
    Process.kill(15, admin)
    Process.kill(15, to_file)
    Process.wait lookupd
    Process.wait d
    Process.wait admin
    Process.wait to_file
  end
end
