class Libgweather < Formula
  desc "GNOME library for weather, locations and timezones"
  homepage "https://wiki.gnome.org/Projects/LibGWeather"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libgweather-4.4.4-1-any.pkg.tar.zst"
  sha256 "2007fc5a0bf4fb57a553eb131427e5bd5926a81b21d21caa44ea5487898c8058"
  license all_of: ["GPL-2.0-or-later", "LGPL-2.1-or-later"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.gnome.org/sources/libgweather/4.6/libgweather-4.6.0.tar.xz"
    strategy :gnome do |page, regex|
      page.scan(regex).select { |match| Version.new(match.first) < 40 }.flatten
    end
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libgweather-4.0/libgweather/gweather.h"
    assert_path_exists lib/"libgweather-4.dll.a"
    assert_path_exists bin/"libgweather-4-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/gweather4.pc").read
  end
end
