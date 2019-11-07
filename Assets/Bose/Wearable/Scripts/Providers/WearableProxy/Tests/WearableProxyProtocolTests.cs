#if UNITY_EDITOR

using NUnit.Framework;
using UnityEngine;

namespace Bose.Wearable.Proxy.Tests
{
	[TestFixture]
	public class WearableProxyProtocolTests
	{
		private string _lastPacketName;

		/// <summary>
		/// Asserts that struct sizes match those specified by the transport protocol
		/// </summary>
		[Test]
		public void AssertStructSizes()
		{
			WearableProxyProtocolBase.AssertCorrectStructSizes();
		}

		/// <summary>
		/// Test roundtrip encoding -> decoding for client-to-server packets
		/// </summary>
		[Test]
		public void TestClientToServer()
		{
			WearableProxyServerProtocol serverProtocol = new WearableProxyServerProtocol();

			int serverIndex = 0;
			byte[] serverBuffer = new byte[1024];

			// Register callbacks for packet processing
			serverProtocol.KeepAlive += () => { _lastPacketName = "KeepAlive"; };
			serverProtocol.PingQuery += () => { _lastPacketName = "PingQuery"; };
			serverProtocol.PingResponse += () => { _lastPacketName = "PingResponse"; };
			serverProtocol.SetNewConfig += deviceConfig =>
			{
				_lastPacketName = "SetConfig";
				Assert.AreEqual(SensorUpdateInterval.ThreeHundredTwentyMs, deviceConfig.updateInterval);
				Assert.AreEqual(false, deviceConfig.rotationSixDof.isEnabled);
				Assert.AreEqual(true, deviceConfig.gyroscope.isEnabled);
				Assert.AreEqual(false, deviceConfig.accelerometer.isEnabled);
				Assert.AreEqual(false, deviceConfig.headNodGesture.isEnabled);
				Assert.AreEqual(true, deviceConfig.doubleTapGesture.isEnabled);
			};
			serverProtocol.QueryConfigStatus += () => { _lastPacketName = "QueryConfig"; };
			serverProtocol.RSSIFilterValueChange += value =>
			{
				_lastPacketName = "SetRSSI";
				Assert.AreEqual(-40, value);
			};
			serverProtocol.InitiateDeviceSearch += () => { _lastPacketName = "StartSearch"; };
			serverProtocol.StopDeviceSearch += () => { _lastPacketName = "StopSearch"; };
			serverProtocol.ConnectToDevice += uid =>
			{
				_lastPacketName = "ConnectToDevice";
				Assert.AreEqual("00000000-0000-0000-0000-000000000000", uid);
			};
			serverProtocol.DisconnectFromDevice += () => { _lastPacketName = "DisconnectFromDevice"; };
			serverProtocol.QueryConnectionStatus += () => { _lastPacketName = "QueryConnection"; };

			// Encode
			WearableProxyProtocolBase.EncodeKeepAlive(serverBuffer, ref serverIndex);
			WearableProxyProtocolBase.EncodePingQuery(serverBuffer, ref serverIndex);
			WearableProxyProtocolBase.EncodePingResponse(serverBuffer, ref serverIndex);

			WearableDeviceConfig config = new WearableDeviceConfig
			{
				updateInterval = SensorUpdateInterval.ThreeHundredTwentyMs,
				gyroscope = {isEnabled = true},
				doubleTapGesture = {isEnabled = true}
			};
			WearableProxyClientProtocol.EncodeSetNewConfig(serverBuffer, ref serverIndex, config);
			WearableProxyClientProtocol.EncodeQueryConfig(serverBuffer, ref serverIndex);

			WearableProxyClientProtocol.EncodeRSSIFilterControl(serverBuffer, ref serverIndex, -40);
			WearableProxyClientProtocol.EncodeInitiateDeviceSearch(serverBuffer, ref serverIndex);
			WearableProxyClientProtocol.EncodeStopDeviceSearch(serverBuffer, ref serverIndex);
			WearableProxyClientProtocol.EncodeConnectToDevice(serverBuffer, ref serverIndex, "00000000-0000-0000-0000-000000000000");
			WearableProxyClientProtocol.EncodeDisconnectFromDevice(serverBuffer, ref serverIndex);
			WearableProxyClientProtocol.EncodeQueryConnectionStatus(serverBuffer, ref serverIndex);
			WearableProxyProtocolBase.EncodeKeepAlive(serverBuffer, ref serverIndex);

			// Decode
			serverIndex = 0;

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("KeepAlive", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("PingQuery", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("PingResponse", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("SetConfig", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("QueryConfig", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("SetRSSI", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("StartSearch", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("StopSearch", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("ConnectToDevice", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("DisconnectFromDevice", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("QueryConnection", _lastPacketName);

			serverProtocol.ProcessPacket(serverBuffer, ref serverIndex);
			Assert.AreEqual("KeepAlive", _lastPacketName);
		}

		/// <summary>
		/// Test roundtrip encoding -> decoding for server-to-client packets
		/// </summary>
		[Test]
		public void TestServerToClient()
		{
			WearableProxyClientProtocol clientProtocol = new WearableProxyClientProtocol();

			int clientIndex = 0;
			byte[] clientBuffer = new byte[1024];

			// Register callbacks for packet processing
			clientProtocol.KeepAlive += () => { _lastPacketName = "KeepAlive"; };

			clientProtocol.NewSensorFrame += frame =>
			{
				_lastPacketName = "SensorFrame";
				Assert.AreEqual(123.45f, frame.timestamp);
				Assert.AreEqual(0.1f, frame.deltaTime);
				Assert.AreEqual(Vector3.right, frame.acceleration.value);
				Assert.AreEqual(SensorAccuracy.Low, frame.acceleration.accuracy);
				Assert.AreEqual(Vector3.up, frame.angularVelocity.value);
				Assert.AreEqual(SensorAccuracy.High, frame.angularVelocity.accuracy);
				Assert.AreEqual(new Quaternion(1.0f, 2.0f, 3.0f, 4.0f), frame.rotationSixDof.value);
				Assert.AreEqual(15.0, frame.rotationSixDof.measurementUncertainty);
				Assert.AreEqual(GestureId.DoubleTap, frame.gestureId);
			};
			clientProtocol.DeviceList += devices =>
			{
				_lastPacketName = "DeviceList";
				Assert.IsTrue(devices.Length == 3);
				Assert.IsTrue(devices[0].name == "Product Name");
				Assert.IsTrue(devices[0].productId == ProductId.Undefined);
				Assert.IsTrue(devices[0].firmwareVersion == "0.13.2f");
				Assert.IsTrue(devices[1].name == "Corey's Device");
				Assert.IsTrue(devices[1].productId == ProductId.Frames);
				Assert.IsTrue(devices[1].variantId == (byte)FramesVariantId.Alto);
				Assert.IsTrue(devices[2].name == "Michael's Headphones");
				Assert.IsTrue(devices[2].productId == ProductId.Frames);
				Assert.IsTrue(devices[2].variantId == (byte)FramesVariantId.Rondo);
			};
			clientProtocol.ConnectionStatus += (state, device) =>
			{
				_lastPacketName = "ConnectionStatus";
				Assert.IsTrue(state == WearableProxyProtocolBase.ConnectionState.Connected);
				Assert.IsTrue(device != null);
				Assert.IsTrue(device.Value.name == "Product Name");
				Assert.AreEqual(ProductId.Frames, device.Value.productId);
				Assert.AreEqual((byte)FramesVariantId.Alto, device.Value.variantId);
				Assert.IsTrue(device.Value.uid == "00000000-0000-0000-0000-000000000000");
			};
			clientProtocol.ConfigStatus += deviceConfig =>
			{
				_lastPacketName = "ConfigStatus";
				Assert.AreEqual(SensorUpdateInterval.ThreeHundredTwentyMs, deviceConfig.updateInterval);
				Assert.AreEqual(false, deviceConfig.rotationSixDof.isEnabled);
				Assert.AreEqual(true, deviceConfig.gyroscope.isEnabled);
				Assert.AreEqual(false, deviceConfig.accelerometer.isEnabled);
				Assert.AreEqual(false, deviceConfig.headNodGesture.isEnabled);
				Assert.AreEqual(true, deviceConfig.doubleTapGesture.isEnabled);
			};

			WearableProxyProtocolBase.EncodeKeepAlive(clientBuffer, ref clientIndex);
			WearableProxyServerProtocol.EncodeSensorFrame(
				clientBuffer, ref clientIndex,
				new SensorFrame
				{
					timestamp = 123.45f,
					deltaTime = 0.1f,
					acceleration = new SensorVector3
					{
						value = Vector3.right,
						accuracy = SensorAccuracy.Low
					},
					angularVelocity = new SensorVector3
					{
						value = Vector3.up,
						accuracy = SensorAccuracy.High
					},
					rotationSixDof = new SensorQuaternion
					{
						value = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f),
						measurementUncertainty = 15.0f
					},
					gestureId = GestureId.DoubleTap
				});
			WearableProxyServerProtocol.EncodeDeviceList(
				clientBuffer, ref clientIndex,
				new[]
				{
					new Device
					{
						isConnected = false,
						name = "Product Name",
						firmwareVersion = "0.13.2f",
						productId = ProductId.Undefined,
						variantId = (byte)FramesVariantId.Undefined,
						rssi = -30,
						uid = "00000000-0000-0000-0000-000000000000"
					},
					new Device
					{
						isConnected = false,
						name = "Corey's Device",
						productId = ProductId.Frames,
						variantId = (byte)FramesVariantId.Alto,
						rssi = -40,
						uid = "00000000-0000-0000-0000-000000000000"
					},
					new Device
					{
						isConnected = false,
						name = "Michael's Headphones",
						productId = ProductId.Frames,
						variantId = (byte)FramesVariantId.Rondo,
						rssi = -55,
						uid = "00000000-0000-0000-0000-000000000000"
					}
				});
			WearableProxyServerProtocol.EncodeConnectionStatus(
				clientBuffer, ref clientIndex,
				WearableProxyProtocolBase.ConnectionState.Connected,
				new Device
				{
					isConnected = true,
					name = "Product Name",
					productId = ProductId.Frames,
					variantId = (byte)FramesVariantId.Alto,
					rssi = -30,
					uid = "00000000-0000-0000-0000-000000000000"
				});
			WearableDeviceConfig config = new WearableDeviceConfig
			{
				updateInterval = SensorUpdateInterval.ThreeHundredTwentyMs,
				gyroscope = {isEnabled = true},
				doubleTapGesture = {isEnabled = true}
			};
			WearableProxyServerProtocol.EncodeConfigStatus(clientBuffer, ref clientIndex, config);
			WearableProxyProtocolBase.EncodeKeepAlive(clientBuffer, ref clientIndex);

			// Decode
			clientIndex = 0;
			clientProtocol.ProcessPacket(clientBuffer, ref clientIndex);
			Assert.AreEqual(_lastPacketName, "KeepAlive");

			clientProtocol.ProcessPacket(clientBuffer, ref clientIndex);
			Assert.AreEqual(_lastPacketName, "SensorFrame");

			clientProtocol.ProcessPacket(clientBuffer, ref clientIndex);
			Assert.AreEqual(_lastPacketName, "DeviceList");

			clientProtocol.ProcessPacket(clientBuffer, ref clientIndex);
			Assert.AreEqual(_lastPacketName, "ConnectionStatus");

			clientProtocol.ProcessPacket(clientBuffer, ref clientIndex);
			Assert.AreEqual(_lastPacketName, "ConfigStatus");

			clientProtocol.ProcessPacket(clientBuffer, ref clientIndex);
			Assert.AreEqual(_lastPacketName, "KeepAlive");
		}
	}
}

#endif
