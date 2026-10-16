class Conmon < Formula
  desc "OCI container runtime monitor"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "conmon", "conmon"
  end

  test do
    assert_match "conmon: Container ID not provided. Use --cid", shell_output("#{bin}/conmon 2>&1", 1)
    (testpath/"bundle/rootfs").mkpath
    (testpath/"bundle/config.json").write <<~JSON
      {
        "ociVersion": "1.2.1",
        "process": { "args": ["cmd.exe", "/c", "echo hello& exit 3"], "env": [], "cwd": "/" },
        "root": { "path": "rootfs" }
      }
    JSON
    # The runtime is wintools' own runc.
    runtime = %W[--runtime #{libexec}/wintools.exe --runtime-arg runc
                 --runtime-arg --root --runtime-arg #{testpath}/state]
    system bin/"conmon", "--cid", "c1", "--cuuid", "u1", *runtime, "--bundle", testpath/"bundle",
           "--log-path", "k8s-file:#{testpath}/ctr.log", "--exit-dir", testpath/"exits"
    system libexec/"wintools.exe", "runc", "--root", testpath/"state", "start", "c1"
    30.times do
      break if (testpath/"exits/u1").exist?

      sleep 1
    end
    assert_equal "3", (testpath/"exits/u1").read.strip
    assert_match "stdout F hello", (testpath/"ctr.log").read
    system libexec/"wintools.exe", "runc", "--root", testpath/"state", "delete", "c1"
  end
end
