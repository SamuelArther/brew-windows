class Umockdev < Formula
  desc "Record device descriptions and run programs under device testbeds"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "umockdev-record", "umockdev-record"
    windows_exe "libexec/wintools.exe", "umockdev-run", "umockdev-run"
    windows_exe "libexec/wintools.exe", "umockdev-wrapper", "umockdev-wrapper"
  end

  def caveats
    <<~EOS
      umockdev-record describes the Windows Plug and Play devices in umockdev's (udev/sysfs) format,
      for bug reports and test fixtures. umockdev-run and umockdev-wrapper build the testbed directory
      from recordings and pass it as UMOCKDEV_DIR; Windows has no LD_PRELOAD, so only programs that
      read the testbed through UMOCKDEV_DIR see the mocked devices. There is no libumockdev C library.
    EOS
  end

  test do
    recording = shell_output("#{bin}/umockdev-record --all")
    assert_match "P: /devices/", recording
    assert_match "E: SUBSYSTEM=", recording
    (testpath/"kbd.umockdev").write "P: /devices/extkeyboard1\nE: SUBSYSTEM=usb\nA: idVendor=0815\n"
    run = shell_output("#{bin}/umockdev-run -d #{testpath}/kbd.umockdev -- cmd /c set UMOCKDEV_DIR")
    assert_match "UMOCKDEV_DIR=", run
    assert_match "UMOCKDEV_DIR=", shell_output("#{bin}/umockdev-wrapper cmd /c set UMOCKDEV_DIR")
    assert_match "0.19.9", shell_output("#{bin}/umockdev-record --version")
  end
end
