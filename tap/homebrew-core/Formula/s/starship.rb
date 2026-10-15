class Starship < Formula
  desc "Cross-shell prompt for astronauts"
  homepage "https://starship.rs/"
  url "https://github.com/starship/starship/releases/download/v1.26.0/starship-x86_64-pc-windows-msvc.zip"
  sha256 "690021e22f2bf2c57d5867dc0fababe5e55e714c26942bd909ffddb6c5df42f2"
  license "ISC"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/starship.exe", "starship"
  end

  test do
    assert_match "starship #{version}", shell_output("#{bin}/starship --version")
  end
end
