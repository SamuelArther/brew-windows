class Container < Formula
  desc "Create and run Linux containers using lightweight virtual machines (WSL2)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "container", "container"
  end

  def caveats
    <<~EOS
      Containers are WSL2 distributions named container-<id>, so WSL must be installed (wsl --install).
      Images and containers are stored in %LOCALAPPDATA%\container (or $CONTAINER_ROOT).
    EOS
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/container --version")
    with_env(CONTAINER_ROOT: (testpath/"store").to_s) do
      assert_match(/ID\s+IMAGE\s+OS\s+ARCH\s+STATE/, shell_output("#{bin}/container list --all"))
      assert_match "NAME", shell_output("#{bin}/container image list")
      assert_match "missing expected argument '<image>'", shell_output("#{bin}/container run 2>&1", 2)
      system bin/"container", "volume", "create", "data"
      assert_path_exists testpath/"store/volumes/data"
      assert_match "data", shell_output("#{bin}/container volume list --quiet")
      assert_match "not found", shell_output("#{bin}/container inspect nope 2>&1", 1)
    end
  end
end
