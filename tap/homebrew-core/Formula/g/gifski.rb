class Gifski < Formula
  desc "Highest-quality GIF encoder based on pngquant"
  homepage "https://gif.ski/"
  url "https://github.com/ImageOptim/gifski/releases/download/1.34.0/gifski-1.34.0.tar.xz"
  sha256 "b9b6591aa163123d737353d9c8581efdf3234d28eeaa45329b31da905cd5a996"
  license "AGPL-3.0-only"

  # Upstream's release archive bundles prebuilt binaries for each OS; the Windows one is win/gifski.exe.

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "win/gifski.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gifski --version")
  end
end
