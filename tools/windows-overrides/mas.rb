class Mas < Formula
  desc "Microsoft Store command-line interface"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mas", "mas"
  end

  def caveats
    <<~EOS
      mas uses Microsoft Store product ids (9WZDNCRFJ3TJ) and installs through winget's msstore source.
    EOS
  end

  test do
    assert_equal version.to_s, shell_output("#{bin}/mas version").chomp
    assert_match "Netflix", shell_output("#{bin}/mas info 9WZDNCRFJ3TJ")
    assert_match "Microsoft Store", shell_output("#{bin}/mas --help")
    assert_match "profile button", shell_output("#{bin}/mas account 2>&1", 1)
  end
end
