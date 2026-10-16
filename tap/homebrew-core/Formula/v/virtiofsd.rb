class Virtiofsd < Formula
  desc "Vhost-user virtio-fs device backend written in Rust"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "virtiofsd", "virtiofsd"
  end

  def caveats
    <<~EOS
      Windows hosts can't run vhost-user devices (no file descriptor passing, no VMM with vhost-user
      virtio-fs); virtiofsd here checks options and reports capabilities. WSL2 already shares Windows folders.
    EOS
  end

  test do
    assert_match "virtiofsd 1.14.0", shell_output("#{bin}/virtiofsd --version")
    assert_match '"type": "fs"', shell_output("#{bin}/virtiofsd --print-capabilities")
    (testpath/"shared").mkpath
    args = "--shared-dir #{testpath}/shared --socket-path #{testpath}/vfs.sock"
    assert_match "can't serve a VM on Windows", shell_output("#{bin}/virtiofsd #{args} 2>&1", 1)
    args = "--shared-dir #{testpath}/nope --socket-path #{testpath}/vfs.sock"
    assert_match "not a directory", shell_output("#{bin}/virtiofsd #{args} 2>&1", 1)
  end
end
