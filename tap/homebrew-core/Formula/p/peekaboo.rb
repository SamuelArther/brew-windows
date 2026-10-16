class Peekaboo < Formula
  desc "Screen capture, UI inspection and automation"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "peekaboo", "peekaboo"
  end

  test do
    ENV["PEEKABOO_CONFIG_DIR"] = (testpath/".peekaboo").to_s
    assert_match(/Peekaboo \d+\.\d+/, shell_output("#{bin}/peekaboo --version"))
    system bin/"peekaboo", "config", "init"
    assert_path_exists testpath/".peekaboo/config.json"
    assert_match "Configuration is valid", shell_output("#{bin}/peekaboo config validate")
    assert_match "no snapshot found", shell_output("#{bin}/peekaboo click --on B1 2>&1", 1)
  end
end
