class Pet < Formula
  desc "Simple command-line snippet manager"
  homepage "https://github.com/knqyf263/pet"
  url "https://github.com/knqyf263/pet/releases/download/v1.0.1/pet_1.0.1_windows_amd64.tar.gz"
  sha256 "49c8346998958774513ee65977d8b9d9a327f76ca3a32caaa30ce1c98dadd64a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/knqyf263/pet/archive/refs/tags/v1.0.1.tar.gz"
  end

  def install
    bin.install "pet.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pet version")
    assert_empty shell_output("#{bin}/pet list")
  end
end
