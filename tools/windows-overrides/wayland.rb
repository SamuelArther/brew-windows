class Wayland < Formula
  desc "Code generator for Wayland protocol XML (wayland-scanner)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "wayland-scanner", "wayland-scanner"
  end

  def caveats
    <<~EOS
      wayland-scanner generates the same C headers and marshalling code as on Linux. The
      libwayland client/server libraries need a Wayland compositor, which Windows runs inside
      WSL (WSLg); build and run Wayland programs there.
    EOS
  end

  test do
    (testpath/"demo.xml").write <<~XML
      <protocol name="demo">
        <interface name="demo_thing" version="2">
          <request name="destroy" type="destructor"/>
          <request name="poke" since="2"><arg name="count" type="uint"/></request>
          <event name="done"><arg name="serial" type="uint"/></event>
        </interface>
      </protocol>
    XML
    system bin/"wayland-scanner", "client-header", testpath/"demo.xml", testpath/"demo-client.h"
    system bin/"wayland-scanner", "private-code", testpath/"demo.xml", testpath/"demo.c"
    assert_match "demo_thing_add_listener", (testpath/"demo-client.h").read
    assert_match "{ \"poke\", \"2u\", demo_types + 0 }", (testpath/"demo.c").read
    assert_match "wayland-scanner", shell_output("#{bin}/wayland-scanner --version 2>&1")
  end
end
