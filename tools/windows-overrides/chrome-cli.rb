class ChromeCli < Formula
  desc "Control Google Chrome from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "chrome-cli", "chrome-cli"
  end

  def caveats
    <<~EOS
      chrome-cli talks to Chrome's DevTools port. Start Chrome with
        chrome --remote-debugging-port=9222 --user-data-dir=<profile folder>
      and set CHROME_CLI_PORT if you use another port.
    EOS
  end

  test do
    assert_match "remote-debugging-port", shell_output("#{bin}/chrome-cli --help")
    port = free_port
    with_env(CHROME_CLI_PORT: port.to_s) do
      assert_match "remote-debugging-port=#{port}", shell_output("#{bin}/chrome-cli list tabs 2>&1", 1)
    end
  end
end
