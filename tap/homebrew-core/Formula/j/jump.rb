class Jump < Formula
  desc "Helps you navigate your file system faster by learning your habits"
  homepage "https://github.com/gsamokovarov/jump"
  url "https://github.com/gsamokovarov/jump/releases/download/v0.69.0/jump_windows_amd64_binary.exe"
  sha256 "ab7900a3d7ed55efee420d019681bada54171814ec4c059752d9f68c80f38671"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gsamokovarov/jump/archive/refs/tags/v0.69.0.tar.gz"
  end

  def install
    bin.install "jump_windows_amd64_binary.exe" => "jump.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jump --version 2>&1")
  end
end
