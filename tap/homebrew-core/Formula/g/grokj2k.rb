class Grokj2k < Formula
  desc "JPEG 2000 Library"
  homepage "https://github.com/GrokImageCompression/grok"
  url "https://github.com/GrokImageCompression/grok/releases/download/v20.4.17/grok-windows-latest.zip"
  sha256 "d1b986bb96d4dd7185a4832058ac1ef6afed0be33e8b09fbc102be65200c9a28"
  license "AGPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/GrokImageCompression/grok/releases/download/v20.4.17/source-full.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/grk_compress.exe", "grk_compress"
    windows_exe "libexec/bin/grk_decompress.exe", "grk_decompress"
    windows_exe "libexec/bin/grk_dump.exe", "grk_dump"
    windows_exe "libexec/bin/grk_transcode.exe", "grk_transcode"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/grk_compress --version 2>&1")
  end
end
