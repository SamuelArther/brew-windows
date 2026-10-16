class Pipewire < Formula
  desc "Audio graph tools for Windows Core Audio (pw-cli, pw-dump, pw-cat, pw-metadata)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "pipewire", "pipewire"
    windows_exe "libexec/wintools.exe", "pw-cli", "pw-cli"
    windows_exe "libexec/wintools.exe", "pw-dump", "pw-dump"
    windows_exe "libexec/wintools.exe", "pw-cat", "pw-cat"
    windows_exe "libexec/wintools.exe", "pw-play", "pw-play"
    windows_exe "libexec/wintools.exe", "pw-record", "pw-record"
    windows_exe "libexec/wintools.exe", "pw-link", "pw-link"
    windows_exe "libexec/wintools.exe", "pw-metadata", "pw-metadata"
  end

  def caveats
    <<~EOS
      The audio server is the Windows Audio service: endpoints are PipeWire nodes (render =
      Audio/Sink, capture = Audio/Source), adapters are devices, and the default metadata is
      Windows' default devices. pw-cat/pw-play/pw-record stream WAV files through WASAPI.
      There is no libpipewire C library on Windows; programs use WASAPI directly.
    EOS
  end

  test do
    assert_match "Compiled with libpipewire", shell_output("#{bin}/pipewire --version")
    assert_match "PipeWire:Interface:Core", shell_output("#{bin}/pw-cli ls Core")
    assert_match "\"type\": \"PipeWire:Interface:Core\"", shell_output("#{bin}/pw-dump")
    assert_match "Found \"default\" metadata", shell_output("#{bin}/pw-metadata")
    shell_output("#{bin}/pw-link -o")
    (testpath/"t.wav").write "RIFF"
    assert_match "no playback target", shell_output("#{bin}/pw-play --target 99999 #{testpath}/t.wav 2>&1", 1)
    shell_output("#{bin}/pw-cat #{testpath}/t.wav 2>&1", 2)
  end
end
