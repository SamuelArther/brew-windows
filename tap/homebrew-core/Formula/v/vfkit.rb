class Vfkit < Formula
  desc "Command-line hypervisor using QEMU on the Windows Hypervisor Platform"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "vfkit", "vfkit"
  end

  def caveats
    <<~EOS
      vfkit runs virtual machines with QEMU's WHPX accelerator. Install QEMU with
        winget install SoftwareFreedomConservancy.QEMU
      (or set VFKIT_QEMU to qemu-system-x86_64.exe) and turn on the
      "Windows Hypervisor Platform" optional feature for hardware speed.
    EOS
  end

  test do
    version_output = shell_output("#{bin}/vfkit --version 2>&1")
    assert_match(/vfkit version: v\d+\.\d+\.\d+/, version_output)
    output = shell_output("#{bin}/vfkit --kernel foo --initrd bar --kernel-cmdline baz 2>&1", 1)
    assert_match("foo: no such file or directory", output)
    touch testpath/"vmlinuz"
    touch testpath/"disk.img"
    cmd = shell_output("#{bin}/vfkit --cpus 2 --memory 1024 --bootloader linux,kernel=#{testpath}/vmlinuz " \
                       "--device virtio-blk,path=#{testpath}/disk.img --device virtio-net,nat --print-qemu-command")
    assert_match "-accel whpx", cmd
    assert_match "-smp 2 -m 1024M", cmd
    assert_match "virtio-blk-pci", cmd
    fs = shell_output("#{bin}/vfkit --kernel #{testpath}/vmlinuz -d virtio-fs,sharedDir=.,mountTag=x 2>&1", 1)
    assert_match "virtio-fs", fs
  end
end
