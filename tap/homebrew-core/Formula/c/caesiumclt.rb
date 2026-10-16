class Caesiumclt < Formula
  desc "Fast and efficient lossy and/or lossless image compression tool"
  homepage "https://github.com/Lymphatus/caesium-clt"
  url "https://github.com/Lymphatus/caesium-clt/releases/download/v1.5.0/caesiumclt-v1.5.0-x86_64-pc-windows-msvc.zip"
  sha256 "4d8469be5d2c0ff07a08cc48a166f6130837c4cbd4cd77fa04cb9f25fd76b790"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Lymphatus/caesium-clt/archive/refs/tags/v1.5.0.tar.gz"
  end

  def install
    bin.install "caesiumclt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/caesiumclt --version 2>&1")
  end
end
