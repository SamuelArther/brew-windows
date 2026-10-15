class Mkcert < Formula
  desc "Simple tool to make locally trusted development certificates"
  homepage "https://github.com/FiloSottile/mkcert"
  url "https://github.com/FiloSottile/mkcert/releases/download/v1.4.4/mkcert-v1.4.4-windows-amd64.exe"
  sha256 "d2660b50a9ed59eada480750561c96abc2ed4c9a38c6a24d93e30e0977631398"
  license "BSD-3-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "mkcert-v1.4.4-windows-amd64.exe" => "mkcert.exe"
  end

  test do
    ENV["CAROOT"] = testpath
    system bin/"mkcert", "brew.test"
    assert_path_exists testpath/"brew.test.pem"
    assert_path_exists testpath/"brew.test-key.pem"
  end
end
