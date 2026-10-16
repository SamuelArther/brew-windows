class Crun < Formula
  desc "Fast and lightweight fully featured OCI runtime and C library"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "crun", "crun"
  end

  def caveats
    <<~EOS
      Containers are Windows processes in job objects (limits, accounting, kill-together); the root
      file system is where the process starts, not a chroot, and Linux (ELF) programs are refused.
      Run Linux images with the container formula (WSL2).
    EOS
  end

  test do
    assert_empty shell_output("#{bin}/crun --root=#{testpath} list -q").strip
    assert_match "crun version", shell_output("#{bin}/crun --version")
    (testpath/"bundle/rootfs/app").mkpath
    (testpath/"bundle/rootfs/app/hello.cmd").write "@echo hello from the rootfs %1\r\n"
    (testpath/"bundle/config.json").write <<~JSON
      {
        "ociVersion": "1.2.1",
        "process": { "args": ["hello", "crun"], "env": ["PATH=/app"], "cwd": "/app" },
        "root": { "path": "rootfs" }
      }
    JSON
    output = shell_output("#{bin}/crun --root=#{testpath}/state run --bundle=#{testpath}/bundle demo")
    assert_match "hello from the rootfs crun", output
  end
end
