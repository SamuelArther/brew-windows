class Lazycontainer < Formula
  desc "Terminal UI for WSL2 containers and images"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "lazycontainer", "lazycontainer"
  end

  def caveats
    <<~EOS
      lazycontainer shows the containers and images of the container formula (WSL2).
    EOS
  end

  test do
    assert_match "lazycontainer version #{version}", shell_output("#{bin}/lazycontainer --version")
    with_env(CONTAINER_ROOT: (testpath/"store").to_s) do
      output = shell_output("#{bin}/lazycontainer")
      assert_match "Containers (0)", output
      assert_match "Images (0)", output
    end
    assert_match "unknown option", shell_output("#{bin}/lazycontainer --bogus 2>&1", 2)
  end
end
