class Cotp < Formula
  desc "TOTP/HOTP authenticator app with import functionality"
  homepage "https://github.com/replydev/cotp"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-cotp-1.10.0-1-any.pkg.tar.zst"
  sha256 "ec0f182adfebfe77dc18e86d5696a417be5dc0c85cc06d92e1993c2052442218"
  license "GPL-3.0-only"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/replydev/cotp/archive/refs/tags/v1.10.0.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/cotp.exe", "cotp"
  end

  test do
    # Proper test needs password input, so use error message for executable check
    assert_match <<~EOS, shell_output("#{bin}/cotp edit 2>&1", 2)
      error: the following required arguments were not provided:
        --index <INDEX>
    EOS

    assert_match version.to_s, shell_output("#{bin}/cotp --version")
  end
end
