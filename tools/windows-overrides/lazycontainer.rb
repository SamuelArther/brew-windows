class Lazycontainer < Formula
  desc "Terminal UI for WSL2 containers and images"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
