class Bluepill < Formula
  desc "Testing tool for iOS that runs UI tests using multiple simulators"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "bluepill", "bluepill"
    windows_exe "libexec/wintools.exe", "bp", "bp"
  end

  def caveats
    <<~EOS
      Running tests needs the iOS Simulator, which is part of Xcode and only runs on macOS.
      On Windows bluepill lists a bundle's tests (-l), plans the simulator bundles from
      --test-time-estimates-json, and merges JUnit reports (--merge-reports DIR).
    EOS
  end

  test do
    assert_match "Usage:", shell_output("#{bin}/bluepill -h")
    assert_match "Usage:", shell_output("#{bin}/bp -h")
    (testpath/"r/sim1").mkpath
    (testpath/"r/sim1/TEST-a.xml").write '<testsuite name="A" tests="2" failures="0" errors="0" time="1.5"><testcase classname="A" name="testX" time="1.5"/></testsuite>'
    assert_match "2 tests", shell_output("#{bin}/bluepill --merge-reports #{testpath}/r")
    assert_match "AllTestUnits", (testpath/"r/TEST-FinalReport.xml").read
    assert_match "invalid option", shell_output("#{bin}/bluepill --bogus 2>&1", 2)
  end
end
