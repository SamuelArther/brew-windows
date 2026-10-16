class Php < Formula
  desc "General-purpose scripting language"
  homepage "https://www.php.net/"
  url "https://windows.php.net/downloads/releases/php-8.5.11-nts-Win32-vs17-x64.zip"
  version "8.5.11"
  sha256 "0ea96e0d2b9b737a6036f05cf4e95c49313faa6d0f27bd97edb2742503f0c043"
  license all_of: [
    "PHP-3.01",
    "Zend-2.0",
    "BSL-1.0",
    "MIT",
    "Apache-1.0",
    "Apache-2.0",
    "bcrypt-Solar-Designer",
    "BSD-2-Clause-Darwin",
    "BSD-2-Clause",
    "BSD-3-Clause",
    "BSD-4-Clause-UC",
    "ISC",
    "LGPL-2.1-only",
    "LGPL-2.1-or-later",
    "OLDAP-2.8",
    "TCL",
    "Zlib",
  ]
  revision 1

  livecheck do
    url "https://windows.php.net/downloads/releases/"
    regex(/href=.*?php[._-]v?(\d+(?:\.\d+)+)[._-]nts[._-]Win32[._-]vs\d+[._-]x64\.zip/i)
  end

  # Official non-thread-safe x64 build from windows.php.net. It needs the
  # Microsoft Visual C++ 2015-2022 redistributable, which the build itself does not include.

  def install
    install_tree
    windows_exe "libexec/php.exe", "php"
    windows_exe "libexec/php-cgi.exe", "php-cgi"
    windows_exe "libexec/phpdbg.exe", "phpdbg"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/php -v")
    assert_match "Homebrew", shell_output("#{bin}/php -r \"echo 'Homebrew';\"")
  end
end
