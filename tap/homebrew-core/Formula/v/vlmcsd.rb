class Vlmcsd < Formula
  desc "KMS Emulator in C"
  homepage "https://github.com/Wind4/vlmcsd"
  url "https://github.com/Wind4/vlmcsd/archive/refs/tags/svn1113.tar.gz"
  version "svn1113"
  sha256 "62f55c48f5de1249c2348ab6b96dabbe7e38899230954b0c8774efb01d9c42cc"
  license "LGPL-2.1-or-later"
  head "https://github.com/Wind4/vlmcsd.git", branch: "master"

  deprecate! date: "2024-12-31", because: :repo_archived
  disable! date: "2025-12-31", because: :repo_archived

  uses_from_macos "llvm" => :build

  def install
    system "make", "CC=clang"
    bin.install "bin/vlmcsd"
    bin.install "bin/vlmcs"
    (etc/"vlmcsd").mkpath
    etc.install "etc/vlmcsd.ini" => "vlmcsd/vlmcsd.ini"
    etc.install "etc/vlmcsd.kmd" => "vlmcsd/vlmcsd.kmd"
    man1.install "man/vlmcs.1"
    man7.install "man/vlmcsd.7"
    man8.install "man/vlmcsd.8"
    man5.install "man/vlmcsd.ini.5"
    man1.install "man/vlmcsdmulti.1"
  end

  def caveats
    <<~EOS
      The default port is 1688

      To configure vlmcsd, edit
        #{etc}/vlmcsd/vlmcsd.ini
      After changing the configuration, please restart vlmcsd
        launchctl unload #{launchd_service_path}
        launchctl load #{launchd_service_path}
      Or, if you don't want/need launchctl, you can just run:
        brew services restart vlmcsd
    EOS
  end

  service do
    run [opt_bin/"vlmcsd", "-i", etc/"vlmcsd/vlmcsd.ini", "-D"]
    keep_alive false
  end

  test do
    output = shell_output("#{bin}/vlmcsd -V")
    assert_match "vlmcsd", output
    output = shell_output("#{bin}/vlmcs -V")
    assert_match "vlmcs", output
    begin
      pid = fork do
        exec bin/"vlmcsd", "-D"
      end
      # Run vlmcsd, then use vlmcs to check
      # the running status of vlmcsd
      sleep 2
      output = shell_output("#{bin}/vlmcs")
      assert_match "successful", output
      sleep 2
    ensure
      Process.kill 9, pid
      Process.wait pid
    end
  end
end
