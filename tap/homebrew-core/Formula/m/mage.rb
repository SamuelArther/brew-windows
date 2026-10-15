class Mage < Formula
  desc "Make/rake-like build tool using Go"
  homepage "https://magefile.org"
  url "https://github.com/magefile/mage/releases/download/v1.17.2/mage_1.17.2_Windows-64bit.zip"
  sha256 "970bc6efa76d6dc7285098a7033f4e6c83c18dc02f80548ae8de8dc5586e0445"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/magefile/mage.git"
  end

  def install
    bin.install "mage.exe"
  end

  test do
    assert_match "magefile.go created", shell_output("#{bin}/mage -init 2>&1")
    assert_path_exists testpath/"magefile.go"
  end
end
