class UmkaLang < Formula
  desc "Statically typed embeddable scripting language"
  homepage "https://github.com/vtereshkov/umka-lang"
  url "https://github.com/vtereshkov/umka-lang/releases/download/v1.5.7/umka_1.5.7_windows_mingw.zip"
  sha256 "9541df53770f92921e2527366f92aadea174d25b30c98516a740513a77bdfd0f"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vtereshkov/umka-lang/archive/refs/tags/v1.5.7.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/umka.exe", "umka"
  end

  test do
    assert_path_exists bin/"umka.exe"
  end
end
