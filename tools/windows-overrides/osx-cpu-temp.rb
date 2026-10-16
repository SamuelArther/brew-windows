class OsxCpuTemp < Formula
  desc "Outputs current CPU temperature"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "osx-cpu-temp", "osx-cpu-temp"
  end

  test do
    assert_match "Fahrenheit", shell_output("#{bin}/osx-cpu-temp -h")
    assert_match(/Num fans: \d+/, shell_output("#{bin}/osx-cpu-temp -f"))
  end
end
