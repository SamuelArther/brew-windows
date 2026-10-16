class Runc < Formula
  desc "CLI tool for spawning and running containers according to the OCI specification"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "runc", "runc"
  end

  def caveats
    <<~EOS
      Containers are Windows processes in job objects (limits, accounting, kill-together); the root
      file system is where the process starts, not a chroot, and Linux (ELF) programs are refused.
      Run Linux images with the container formula (WSL2).
    EOS
  end

  test do
    system bin/"runc", "spec", "--bundle", testpath
    assert_path_exists testpath/"config.json"
    (testpath/"bundle/rootfs").mkpath
    (testpath/"bundle/config.json").write <<~JSON
      {
        "ociVersion": "1.2.1",
        "process": { "args": ["cmd.exe", "/c", "echo container says %FOO%"], "env": ["FOO=hi"], "cwd": "/" },
        "root": { "path": "rootfs" },
        "linux": { "resources": { "memory": { "limit": 268435456 }, "pids": { "limit": 8 } } }
      }
    JSON
    state = testpath/"state"
    assert_match "container says hi", shell_output("#{bin}/runc --root #{state} run --bundle #{testpath}/bundle c1")
    system bin/"runc", "--root", state, "create", "--bundle", testpath/"bundle", "c2"
    assert_match '"status": "created"', shell_output("#{bin}/runc --root #{state} state c2")
    system bin/"runc", "--root", state, "delete", "--force", "c2"
    assert_empty shell_output("#{bin}/runc --root #{state} list -q").strip
    assert_match "does not exist", shell_output("#{bin}/runc --root #{state} kill c9 KILL 2>&1", 1)
  end
end
