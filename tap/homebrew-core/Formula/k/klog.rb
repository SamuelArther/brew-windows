class Klog < Formula
  desc "Command-line tool for time tracking in a human-readable, plain-text file format"
  homepage "https://klog.jotaen.net"
  url "https://github.com/jotaen/klog/releases/download/v7.1/klog-windows.zip"
  sha256 "b6b70e470a8c79f2add17f54c329988cd9fc6acbc4135807f2f51aa8149d8803"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jotaen/klog/archive/refs/tags/v7.1.tar.gz"
  end

  def install
    bin.install "klog.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/klog version --no-check --quiet")

    (testpath/"test.klg").write <<~EOS
      2018-03-24
      First day at my new job
          8:30 - 17:00
          -45m Lunch break
    EOS

    assert_match "Total: 7h45m", shell_output("#{bin}/klog total --no-style #{testpath}/test.klg")
  end
end
